// Component Imports
import UserDetailView from '@views/users/UserDetailView'

const UserDetailPage = ({ params }: { params: { userId: string } }) => {
  return <UserDetailView userId={params.userId} />
}

export default UserDetailPage
